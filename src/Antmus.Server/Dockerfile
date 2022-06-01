FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /build

FROM build AS publish
RUN dotnet publish -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=publish /publish .

ENV Logging__Console__FormatterName simple
ENV ASPNETCORE_URLS http://+:5000
ENV Mocks__Path /mocks

EXPOSE 5000

ENTRYPOINT ["dotnet", "Antmus.Server.dll"]