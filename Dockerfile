FROM microsoft/dotnet
WORKDIR /src
COPY /src/out .
COPY /src/dk-postnr.csv .
COPY /src/no-postnr.csv .
COPY /src/de_postal_codes.csv .
ENTRYPOINT ["dotnet", "Alice.dll"]
EXPOSE 5000
HEALTHCHECK CMD curl --fail http://localhost:5000/api/health || exit 1
