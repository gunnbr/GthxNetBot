#!/bin/sh
# wait-for-sql.sh

set -e

host="localhost"
port="1433"
user="sa"
password="Your_password123"
timeout=60
sql_ready=0

echo "Waiting for SQL Server to be available..."

for i in $(seq 1 $timeout); do
    if /opt/mssql-tools/bin/sqlcmd -S $host,$port -U $user -P $password -Q "SELECT 1" > /dev/null 2>&1; then
        sql_ready=1
        break
    fi
    echo "SQL Server not ready yet... ($i/$timeout)"
    sleep 1
done

if [ "$sql_ready" -ne 1 ]; then
    echo "SQL Server did not become available in time."
    exit 1
fi

echo "SQL Server is up!"
