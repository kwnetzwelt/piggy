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
      # Uncomment to disable 2FA (e.g. if you lose access to your authenticator app):
      # - Disable2FA=true
      # Uncomment when running behind a reverse proxy that terminates TLS:
      # - ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
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

## Two-Factor Authentication

After logging in via OIDC, you'll be prompted for a TOTP code from your authenticator app.

**Setup:** Go to **Settings → Two-Factor Auth**, scan the QR code, and confirm with a code.

**Reset if you lose access:** Add `Disable2FA=true` to the piggy environment, restart the container, reconfigure your authenticator from **Settings → Two-Factor Auth**, then remove the env var and restart.
