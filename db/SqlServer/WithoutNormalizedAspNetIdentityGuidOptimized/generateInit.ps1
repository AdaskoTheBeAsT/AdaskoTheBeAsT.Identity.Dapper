# below 4 commented lines needs to be run only one on given machine
# to install new project type
# to restore all tools without which this script will not work
# to trust PSGallery
# and install sqlserver module from it

#dotnet new -i Microsoft.Build.Sql.Templates
#dotnet tool restore
#Set-PSRepository -Name "PSGallery" -InstallationPolicy Trusted
#Install-Module sqlserver -AllowClobber

$Env:DOTNET_CLI_UI_LANGUAGE = "en"

$ServerName = "localhost\SQLEXPRESS"
$DatabaseName = "WithoutNormalizedAspNetIdentityGuidOptimized"


# regenerating database on local machine - removing old db
$ConnectionStringMaster = "Server=$ServerName;Database=master;Integrated Security=True;TrustServerCertificate=True;Encrypt=yes;"
$ConnectionStringTest = "Server=$ServerName;Database=$DatabaseName;Integrated Security=True;TrustServerCertificate=True;Encrypt=yes;"

$QueryCheckExistence = "IF EXISTS (SELECT name FROM sys.databases WHERE name='$DatabaseName') SELECT 1 ELSE SELECT 0"

$DbExists = Invoke-Sqlcmd -Query $QueryCheckExistence -ConnectionString $ConnectionStringMaster

if ($DbExists.Column1 -eq 1) {
    $QueryDropDatabase = "DROP DATABASE $DatabaseName"
    Invoke-Sqlcmd -Query $QueryDropDatabase -ConnectionString $ConnectionStringMaster
    Write-Output "Database $DatabaseName dropped."
} else {
    Write-Output "Database $DatabaseName does not exists"
}

# clean debug folder
Get-ChildItem ./bin/Debug -File | Remove-Item -Force -Recurse -Confirm:$false


# build database project
dotnet build "./WithoutNormalizedAspNetIdentityGuidOptimized.sqlproj" -p:Configuration=Debug

# prepare publish script
dotnet sqlpackage /Action:Script /SourceFile:"./bin/Debug/WithoutNormalizedAspNetIdentityGuidOptimized.dacpac" `
    /OutputPath:"./bin/Debug/WithoutNormalizedAspNetIdentityGuidOptimized.publish.sql" `
    /TargetConnectionString:"$ConnectionStringTest"

# deploy to local sql server project to check if everything is ok
dotnet sqlpackage /Action:Publish /SourceFile:"./bin/Debug/WithoutNormalizedAspNetIdentityGuidOptimized.dacpac" `
    /TargetConnectionString:"$ConnectionStringTest"

# copy init script to integ test
Copy-Item "./bin/Debug/WithoutNormalizedAspNetIdentityGuidOptimized.publish.sql" "../../../test/integ/AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest/Scripts/WithoutNormalizedAspNetIdentityGuidOptimized/init.sql" -Force

# clean bin and obj folder
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force
