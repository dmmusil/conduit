docker-compose stop -t 1
docker-compose rm -f
docker-compose pull
docker-compose up --build -d
dotnet test
# if [ $? -ne 0 ]
# then
#     docker-compose stop -t 1
#     exit 1
# fi
# docker-compose stop -t 1