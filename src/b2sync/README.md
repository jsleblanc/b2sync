# Build

```
docker buildx prune
docker compose build --no-cache
docker save b2sync:latest -o b2sync.img

docker build -t b2sync -f Dockerfile . --no-cache
```
