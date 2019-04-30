FROM microsoft/dotnet:2.2-sdk-alpine AS build

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
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build /build/out ./

EXPOSE 5000
ENTRYPOINT ["dotnet", "Peekaqueue.dll"]