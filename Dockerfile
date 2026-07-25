# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY src/Piggy/Piggy.csproj src/Piggy/
RUN dotnet restore src/Piggy/Piggy.csproj
COPY src/Piggy/ src/Piggy/
RUN dotnet publish src/Piggy/Piggy.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
ARG BUILD_TAG
WORKDIR /app
COPY --from=build /app/publish .
RUN echo "${BUILD_TAG:-development}" > /app/version.txt
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Piggy.dll"]
