# this script can be run directly or from the github actions.

# set input or default

DEFAULTSEMVER="1.0.0-Test-DATE-TIME"
NUGETSEMVER=${1:-$DEFAULTSEMVER}

# replace date
DATE_SHORT=$(date +'%Y%m%d')
NUGETSEMVER="${NUGETSEMVER/DATE/$DATE_SHORT}"

# replace time
TIME_SHORT=$(date +'%H%M%S')
NUGETSEMVER="${NUGETSEMVER/TIME/$TIME_SHORT}"

# report semver
echo "Semver: $NUGETSEMVER";

# build

dotnet restore

dotnet pack -c Release --output "." -p:PackageVersion=$NUGETSEMVER