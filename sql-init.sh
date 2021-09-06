sleep 90s
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P P@ssw0rd -d master -Q "create database conduit; go;"
