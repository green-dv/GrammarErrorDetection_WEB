# Blazor WebAssembly: se compila a archivos estaticos y nginx los sirve.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero los .csproj, para que el restore se cachee entre builds.
COPY GrammarErrorDetection.Models/*.csproj GrammarErrorDetection.Models/
COPY GrammarErrorDetection.Infraestructure/*.csproj GrammarErrorDetection.Infraestructure/
COPY GrammarErrorDetection.Services/*.csproj GrammarErrorDetection.Services/
COPY GrammarErrorDetection.WEB/*.csproj GrammarErrorDetection.WEB/
RUN dotnet restore GrammarErrorDetection.WEB/GrammarErrorDetection.WEB.csproj

COPY GrammarErrorDetection.Models/ GrammarErrorDetection.Models/
COPY GrammarErrorDetection.Infraestructure/ GrammarErrorDetection.Infraestructure/
COPY GrammarErrorDetection.Services/ GrammarErrorDetection.Services/
COPY GrammarErrorDetection.WEB/ GrammarErrorDetection.WEB/
RUN dotnet publish GrammarErrorDetection.WEB/GrammarErrorDetection.WEB.csproj \
    -c Release --no-restore -o /out

FROM nginx:stable-alpine AS final
COPY deploy/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /out/wwwroot/ /usr/share/nginx/html/
EXPOSE 80
