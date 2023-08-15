# NetCore项目(集成EF)

该CI脚本组，适用于使用EF进行数据库维护的NetCore项目，该脚本将会执行dotnet ef update等命令。
该项目在执行UT Test的过程中会使用Staging配置项中的数据库

## 如何使用该Example

1. 创建Gitlab项目后，将``pod.yaml``和``.gitlab-ci.yml``放置在Repo的根目录
2. 将``Dockerfile``文件放置在项目主目录下，和csproj文件同一层。
3. 确认项目有一个单元测试项目，名为``MsTest``
3. 编辑本Repo根目录的``ci_config_netcore.csv``，为新项目加入一行记录。
4. 添加跨域设置如下：`` if (env.IsStaging()) { app.UseCors(c => c.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()); } ``
5. 在阿里云的容器镜像服务里，在``yanzhonglan``命名空间下，创建一个repo，起名为csv文件中的``ProjectShort``字段值
6. 在阿里云的Kubernetes里，创建一个Dep，其名称为ProjectShort字段值，初始内容随意添加。
7. 如果项目有特别的Pod设置，修改Pod文件加入此设置，默认情况下，Pod文件中仅包含空白NetCore项目运行所需的设置。
8. 修改Gitlab的Ci中的Variable，加入``KUBERNETES_CONFIGURATION``，``PRODUCTION_APPSETTINGS``两个File类型的变量，注意不要Protect
9. 加入可选的其他变量，包括：``ALIYUN_SLS_LOGGER_SECRET``
10. 修改NetCore项目，使其监听http://0.0.0.0:5000
11. 需要至少提供``appsettings.Staging.json`` 配置文件，且不建议提供``appsettings.Production.json``文件（该文件如果有，会被环境变量覆盖）。
12. 在Gitlab创建分支``production``，然后编辑Protected Branch规则，修改如下
    * 修改master为：任何人都可以合并，任何人不可以推送。
    * 修改production为：只有管理员可以合并，任何人不可推送。
13. 在Gitlab创建Protected Tags规则``release-*``只能由管理员打（这些Tag将由部署脚本自动打，用户为root）
14. 确保在Staging环境下，配置的数据库密码为``my_pw``

## 如何进行日常开发

日常开发时，程序员在自己的branch下进行开发，需要推送到Staging服务器时，只需创建一个MergeRequest，合并到master分支，即可自动触发CI将其推送到Staging。  
当需要发布生产时，创建一个MergeRequest，从``master``合并到``production``，即可触发CI将镜像更新到生产环境，同时创建Tag。