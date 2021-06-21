# Example client library for omnikeeper in Powershell

## Requirements
- PowerShell 6.2 or later

## setup/install
To install from the source, run the following command to download, build and install the PowerShell module locally (cd to this directory first):
```powershell
git clone https://www.mhx.at/gitlab/landscape/omnikeeper-client-powershell.git
cd omnikeeper-client-powershell
.\Build.ps1
Import-Module -Name '.\src\okclient' -Verbose
cd ..
```

## Running example 
```powershell
.\example.ps1
```
Should return a list of CIIDs from the acme-dev omnikeeper instance.

## Authentication
Authentication is done via username + password. Make sure chosen user exists in keycloak.