# Docker Compose

The `docker-compose.yml` file is not tracked in this repo. Create your own with this structure:

```yaml
services:
  piggy:
    build: .
    ports:
      - "5000:8080"
    environment:
      - MongoDb__ConnectionString=mongodb://mongo:27017
      - MongoDb__DatabaseName=piggy
      - Oidc__Authority=https://your-oidc-provider
      - Oidc__ClientId=your-client-id
      - Oidc__ClientSecret=your-client-secret
    depends_on:
      - mongo

  mongo:
    image: mongo:7
    ports:
      - "27017:27017"
    volumes:
      - mongo-data:/data/db

volumes:
  mongo-data:
```
