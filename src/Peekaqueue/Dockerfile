FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build

ARG BUILDCONFIG=RELEASE
ARG VERSION=1.0.0

# copy csproj and restore as distinct layers
COPY Peekaqueue.csproj ./build/
RUN dotnet restore build/Peekaqueue.csproj

# copy everything else and build
WORKDIR /build/
COPY ./ ./
RUN dotnet publish -c $BUILDCONFIG -o out /p:Version=$VERSION

# build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine
WORKDIR /app
COPY --from=build /build/out ./

EXPOSE 5000
ENTRYPOINT ["dotnet", "Peekaqueue.dll"]