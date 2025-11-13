ARG VERSION=10.0-alpine

FROM mcr.microsoft.com/dotnet/aspnet:$VERSION AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:$VERSION AS build
WORKDIR /src

RUN apk add --no-cache nodejs npm

COPY ["package.json", "package-lock.json*", "./ClientBooking/"]
WORKDIR "/src/ClientBooking"
RUN npm install

COPY ["ClientBooking/ClientBooking.csproj", "ClientBooking/"]
RUN dotnet restore "ClientBooking/ClientBooking.csproj"

COPY . .
WORKDIR "/src/ClientBooking"

RUN dotnet build "ClientBooking/ClientBooking.csproj" -c Release -o /app/build
RUN npx tailwindcss -i ClientBooking/wwwroot/app.css -o ClientBooking/wwwroot/styles.css --minify

FROM build AS publish
RUN dotnet publish "ClientBooking/ClientBooking.csproj" -c Release -o /app/publish -p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClientBooking.dll"]
