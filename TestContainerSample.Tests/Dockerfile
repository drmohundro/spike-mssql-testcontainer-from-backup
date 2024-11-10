FROM mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04 AS setup

# See https://stackoverflow.com/a/69494857/4570

# TODO: perhaps drop this, but it does make it convenient...
ENV ACCEPT_EULA=Y
ENV MSSQL_SA_PASSWORD="Password1234!"

FROM setup AS data
COPY --chmod=0755 Backups/SampleDatabase.bak /
RUN ( /opt/mssql/bin/sqlservr & ) | grep -q "Service Broker manager has started" \
    && /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $MSSQL_SA_PASSWORD -C -Q 'RESTORE DATABASE SampleDatabase FROM DISK = "/SampleDatabase.bak" WITH MOVE "SampleDatabase" to "/var/opt/mssql/data/SampleDatabase.mdf", MOVE "SampleDatabase_Log" to "/var/opt/mssql/data/SampleDatabase_log.ldf", NOUNLOAD, STATS = 5' \
    && pkill sqlservr

FROM setup

# Copy the prepopulated data tree, but not the backup file
COPY --from=data /var/opt/mssql /var/opt/mssql