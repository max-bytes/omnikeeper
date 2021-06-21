$BaseURL = "https://acme.omnikeeper-dev.bymhx.at/backend"
$TokenURL = "https://auth-dev.mhx.at/auth/realms/acme/protocol/openid-connect/token"
$Username = "omnikeeper-client-library-test"
$Password = "omnikeeper-client-library-test"
$APIVersion = "1"

$body = @{
    grant_type='password'
    username=$Username
    password=$Password
    client_id='landscape-omnikeeper'
}

# Token Request
$tokenResponse = Invoke-WebRequest -Method POST -Uri $TokenURL -ContentType "application/x-www-form-urlencoded" -Body $body -Credential $credentials | ConvertFrom-Json
$accessToken = $tokenResponse.access_token

#Write-Host $accessToken

$Configuration = Get-OKConfiguration
$Configuration.BaseUrl = $BaseURL

# NOTE: the powershell api generator does not properly support access tokens
# that's why we set the Authorization header manually
# $Configuration.AccessToken = $accessToken
$Configuration.DefaultHeaders = @{
    Authorization = "Bearer ${accessToken}"
}

try {
    $Result = Get-OKAllCIIDs -Version $APIVersion
    Write-Host($Result)
} catch {
    Write-Host ("Exception occured when calling Get-OKAllCIIDs: {0}" -f ($_.ErrorDetails))
    Write-Host ("Response headers: {0}" -f ($_.Exception.Response.Headers | ConvertTo-Json))
}
