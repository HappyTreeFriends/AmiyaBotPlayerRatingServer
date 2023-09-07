我想要使用ASP.NET Core Identity搭建一个用户管理系统，用户将有三种身份，管理员账户,开发者账户和普通账户，我的前端界面将是前后端分离的vue界面，现在仅讨论后端.
目前,我已经使用Microsoft.AspNetCore.Identity.EntityFrameworkCore，Microsoft.AspNetCore.Authentication.JwtBearer搭配我的Postgresql数据库，完成了用户的注册，登录，授权。
目前已经测试并确认[Authorize(Roles = "管理员账户,开发者账户")]可以正常工作
我接下来将会据此询问一系列问题。

我现在希望开发者账户可以申请一组ApiSecret和ApiKey（比如OpenIddict的ClientId和ClientSecret）。
然后提供一组API，需要用ApiKey和ApiSecret来访问。
并且我希望这个ApiKey可以让开发者账户选择“获取数据”和“写入数据”两种权限中的一种或者两种。
我想要使用OpenIddict的Client Credentials Grant来实现上述功能，同时还能不和我我前面开发的jwt用户登录冲突
请从OpenIddict的安装开始，请给出实现这个功能的步骤。