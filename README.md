# 兔兔集成角色练度管理系统

目前的计划

1、用户能在我的网站注册“开发者账户”，通过开发者账户拿到ApiKey和Secret，然后可以通过这个Key和Secret来提交他们所收集的用户练度数据。还可以可以注册一个OAUTH client，通过他来获取用户数据。
2、开发者账户可以登录网站后台，通过网页来管理Key和其他一些信息。

3、还有另一些可以公开访问的API，他们不受Secret的限制，可以访问一些诸如练度统计之类的数据。
4、网站还有可以匿名访问的网页，他会调用那些公开的API来生成内容。

5、网站还可以注册“普通用户”账户，他们可以通过这个页面管理Cred，查看失效情况，或者如果有隐私安全隐患的话，仅仅管理box json。
6、“开发者账户”可以在他们的网站，通过OAuth的形式，授权我们向他们提供“普通用户”他的box json。
7、“开发者账户”可以在他们的网站，引导用户在我们的网站注册账户，上传cred，或者上传box，这样开发者就不需要再考虑森空岛api相关的问题，而是可以直接通过我们的api来提取数据，并且多个数据源可以共享。

>%USERPROFILE%\.dotnet\tools\dotnet-ef migrations add 

# 本周的Roadmap

实现用户注册与登录
实现用户申请Secret的API(包括Secrert的列出,申请与作废,并且提交box和获取数据权限分开)
实现用户通过API提交用户box
实现公开的API可以访问统计数据,包含按日期范围统计的数据
实现普通用户提交自己的box或cred,以及管理的API
开发UI包括注册,登录,提交box和cred
实现开发者账户OAuth申请用户的box
实现开发者UI

