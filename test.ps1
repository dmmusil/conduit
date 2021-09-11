docker-compose stop -t 1
docker-compose rm -f
docker-compose pull
docker-compose up --build -d
dotnet test
