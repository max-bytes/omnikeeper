#!/usr/bin/env sh

./DBMigrate/DBMigrate "$ConnectionStrings__LandscapeDatabaseConnection"

dotnet Omnikeeper.dll