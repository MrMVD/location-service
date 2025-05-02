# Location Service REST API

Microservice for selecting advertising platforms for a specific region


## 🚀 quick start

1. download or build a docker image
2. download docker-compose.yml
3. run with docker-compose run

## .NET start

1. download the release or build the project
2. run with .\LocationService.exe --urls "http://address:port"
(You can omit the argument if you want to run with default parameters)

## 📚 API

### POST /api/advertising/upload
Loading site data from file

**Parameters:**
* file: TXT-file with companies and their regions (format: firm1:/path1, firm1:/path1/path12,/path2)

### GET /api/advertising/search
Search companies by location

**Parameters:**
* location: location to search (example: /path1/path12)

## Data storage

The data is stored in a custom implementation of a thread-safe prefix tree, where locations are stored instead of symbols.

## License
MIT

