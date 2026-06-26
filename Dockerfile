# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore as a separate layer so dependency changes don't bust the cache on every code edit.
COPY src/InsuranceManagement.Api/InsuranceManagement.Api.csproj src/InsuranceManagement.Api/
RUN dotnet restore src/InsuranceManagement.Api/InsuranceManagement.Api.csproj

# Copy the rest of the source and publish.
COPY src/ src/
RUN dotnet publish src/InsuranceManagement.Api/InsuranceManagement.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "InsuranceManagement.Api.dll"]
