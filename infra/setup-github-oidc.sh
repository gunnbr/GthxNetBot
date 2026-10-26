#!/bin/sh
# Lets this repo's GitHub Actions deploy to Azure without a stored password (OpenID Connect).
#
# Creates an app registration and service principal that trust only tokens for this repo's master branch
# (the build job) and its "production" environment (the deploy job), and gives it:
#   - Contributor on the resource group
#   - AcrPush on the container registry
#   - Role Based Access Control Administrator on the resource group, limited by a condition to assigning
#     only the roles main.bicep creates (AcrPull, Key Vault Secrets User, Key Vault Secrets Officer)
#
# Run it after the bootstrap deploy (infra/README.md, step 2), signed in as an Owner of the subscription.
#
#   infra/setup-github-oidc.sh [resource group] [owner/repo]
set -eu
RESOURCE_GROUP=${1:-rg-gthx-prod}
REPO=${2:-gunnbr/GthxNetBot}
APP_NAME=gthx-github-deploy

ACR_PULL=7f951dda-4ed3-4680-a7ca-43fe172d538d
KV_SECRETS_USER=4633458b-17de-408a-b874-0445c86b69e6
KV_SECRETS_OFFICER=b86a8fe4-44ce-4948-aee5-eccb2c155cd7
ROLES="$ACR_PULL, $KV_SECRETS_USER, $KV_SECRETS_OFFICER"

SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)
RG_ID=$(az group show -n "$RESOURCE_GROUP" --query id -o tsv)
REGISTRY_ID=$(az acr list -g "$RESOURCE_GROUP" --query '[0].id' -o tsv)
if [ -z "$REGISTRY_ID" ]; then
    echo "No container registry in $RESOURCE_GROUP; run the bootstrap deploy first." >&2
    exit 1
fi

APP_ID=$(az ad app list --display-name "$APP_NAME" --query '[0].appId' -o tsv)
if [ -z "$APP_ID" ]; then
    APP_ID=$(az ad app create --display-name "$APP_NAME" --query appId -o tsv)
fi
SP_ID=$(az ad sp list --filter "appId eq '$APP_ID'" --query '[0].id' -o tsv)
if [ -z "$SP_ID" ]; then
    SP_ID=$(az ad sp create --id "$APP_ID" --query id -o tsv)
fi

add_credential() {
    name=$1
    subject=$2
    if [ -z "$(az ad app federated-credential list --id "$APP_ID" --query "[?name=='$name'].name" -o tsv)" ]; then
        az ad app federated-credential create --id "$APP_ID" --parameters "{
            \"name\": \"$name\",
            \"issuer\": \"https://token.actions.githubusercontent.com\",
            \"subject\": \"$subject\",
            \"audiences\": [\"api://AzureADTokenExchange\"]
        }" >/dev/null
    fi
}
add_credential github-master "repo:$REPO:ref:refs/heads/master"
add_credential github-production "repo:$REPO:environment:production"

assign() {
    az role assignment create --assignee-object-id "$SP_ID" --assignee-principal-type ServicePrincipal "$@" >/dev/null
}
assign --role Contributor --scope "$RG_ID"
assign --role AcrPush --scope "$REGISTRY_ID"
assign --role "Role Based Access Control Administrator" --scope "$RG_ID" --condition-version 2.0 --condition "(
 (
  !(ActionMatches{'Microsoft.Authorization/roleAssignments/write'})
 )
 OR
 (
  @Request[Microsoft.Authorization/roleAssignments:RoleDefinitionId] ForAnyOfAnyValues:GuidEquals {$ROLES}
 )
)
AND
(
 (
  !(ActionMatches{'Microsoft.Authorization/roleAssignments/delete'})
 )
 OR
 (
  @Resource[Microsoft.Authorization/roleAssignments:RoleDefinitionId] ForAnyOfAnyValues:GuidEquals {$ROLES}
 )
)"

cat <<EOF

Done. Set these in GitHub (Settings > Secrets and variables > Actions):

  Variables:
    AZURE_CLIENT_ID=$APP_ID
    AZURE_TENANT_ID=$TENANT_ID
    AZURE_SUBSCRIPTION_ID=$SUBSCRIPTION_ID
    AZURE_RESOURCE_GROUP=$RESOURCE_GROUP
  Secret:
    BUDGET_CONTACT_EMAIL=<where budget alerts go>

Then create the "production" environment with yourself as a required reviewer.
EOF
