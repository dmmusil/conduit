docker-compose -f docker-compose.yml -f docker-compose-$(arch).yml stop -t 1
docker-compose -f docker-compose.yml -f docker-compose-$(arch).yml rm -f
docker-compose pull
docker-compose -f docker-compose.yml -f docker-compose-$(arch).yml up --build -d
