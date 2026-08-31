# Imagen base del runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Librerías nativas que Npgsql intenta cargar en runtime (Kerberos / GSSAPI).
# Sin esto sale el warning libgssapi_krb5.so.2 cannot open shared object file.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# Imagen del SDK para build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Abril-Backend.csproj", "./"]
RUN dotnet restore "Abril-Backend.csproj"
COPY . .
RUN dotnet publish "Abril-Backend.csproj" -c Release -o /app/publish

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Abril-Backend.dll"]