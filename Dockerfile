FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY ./publish .
RUN ls -ltr 
ENV TZ=Asia/Shanghai
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# 安装git命令
RUN apt-get update && apt-get install -y git

CMD ["dotnet", "AmiyaBotPlayerRatingServer.dll"]
