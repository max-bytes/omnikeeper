#!/usr/bin/env sh

./DBMigrate/DBMigrate "$ConnectionStrings__LandscapeDatabaseConnection"

dotnet LandscapeRegistry.dll