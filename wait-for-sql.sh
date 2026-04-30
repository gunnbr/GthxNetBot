#!/bin/sh
# wait-for-sql.sh

set -e

host="localhost"
port="1433"
user="sa"
password="Your_password123"
timeout=60

echo "Waiting for SQL Server to be available..."

for i in $(seq 1 $timeout); do
    /opt/mssql-tools/bin/sqlcmd -S $host,$port -U $user -P $password -Q "SELECT 1" > /dev/null 2>&1 && break
    echo "SQL Server not ready yet... ($i/$timeout)"
    sleep 1
done

if [ $i -eq $timeout ]; then
    echo "SQL Server did not become available in time."
    exit 1
fi

echo "SQL Server is up!"
