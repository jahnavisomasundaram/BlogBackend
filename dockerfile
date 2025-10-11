# ===============================
# BUILD STAGE
# ===============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything and restore dependencies
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# ===============================
# RUNTIME STAGE
# ===============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Expose the port Render will use
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Entry point — make sure your project builds to BlogBackend.dll
ENTRYPOINT ["dotnet", "BlogAppBackend.dll"]
