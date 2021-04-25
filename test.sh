docker-compose rm -f
docker-compose pull
docker-compose up --build -d
dotnet test
docker-compose stop -t 1