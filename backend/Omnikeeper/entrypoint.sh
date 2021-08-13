#!/usr/bin/env sh

./DBMigrate/DBMigrate "$ConnectionStrings__OmnikeeperDatabaseConnection"

dotnet Omnikeeper.dll