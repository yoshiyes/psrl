FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Passerelle.slnx ./
COPY Directory.Packages.props ./
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Host/Host.csproj src/Host/

RUN dotnet restore Passerelle.slnx

COPY . .

RUN dotnet publish src/Host/Host.csproj \
    --configuration Release \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Passerelle.Host.dll"]