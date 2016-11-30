FROM microsoft/dotnet
WORKDIR /src/Minerva
COPY /src/Minerva/out .
ENTRYPOINT ["dotnet", "Minerva.dll"]
EXPOSE 5000