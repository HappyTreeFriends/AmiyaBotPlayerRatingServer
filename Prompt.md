# Summary

这是一段Controller的代码,请去去掉不重要的内容,仅保留函数原型和一个典型返回值,便于向生成式AI提问.示例返回值请至少包含各个字段名和一个字段典型值. 注意保留HttpMethod和Route方便AI了解具体路径

# 主线任务

我想要使用ASP.NET Core Identity搭建一个用户管理系统，用户将有三种身份，管理员账户,开发者账户和普通账户，我的前端界面将是前后端分离的vue界面.
目前,我已经使用Microsoft.AspNetCore.Identity.EntityFrameworkCore，Microsoft.AspNetCore.Authentication.JwtBearer搭配我的Postgresql数据库，完成了用户的注册，登录，授权的Api。
我已经使用OpenIddict实现了创建Client的Api

目前已经测试并确认
1、[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "管理员账户,开发者账户")]可以正常工作
2、[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,Policy = "ReadData")]可以正常工作
我接下来将会据此询问一系列问题。

我现在想让普通用户可以进行注册，并管理一个叫做森空岛(SKLand) Credential的字符串。
(注意用户注册和通过登录获取JwtToken的Api已经实现了)
通过这个Cred可以获取玩家在森空岛的昵称，头像，角色列表（character box， Json格式）。
我希望设计一个vue页面，可以让用户注册，登录，提交Cred，更新Cred，查看每个Cred对应box。
在列表中区分Cred时，使用昵称和头像来区分他们。
现在请设计一系列Controller，实现上述功能，请仅提供每个Controller的类名，和里面包含的Action函数声明，不需要提供实现。
这些Controller放在AmiyaBotPlayerRatingServer.Controllers命名空间下,必要时可以使用子命名空间分类.

现在我想搭建这个基于vue的网站,要求是Vue3,Vite,TypeScript,组合式Api风格.
我现在后端的Api已经编写完成,在我提供这些api之前,能否给出创建空项目的步骤?

const headers = {
      "Cred": token
    }

    let response = await axios.get('https://zonai.skland.com/api/v1/user/me', {
      headers: headers
    });

    console.log(response)

    if (response.status != 200 || response.data.code != 0) {
      return false
    }

    let meData = response.data.data

    doctorScore.value.name = meData.gameStatus.name

    response = await axios.get('https://zonai.skland.com/api/v1/game/player/info?uid=' + meData.gameStatus.uid, {
      headers: headers
    });

    console.log(response)

    if (response.status != 200 || response.data.code != 0) {
      return false
    }


    let infoData = response.data.data

这是一段js代码，根据一个字符串 token，获取一个json的object infoData

现在请将其改写为一个hangfire任务，用于在asp.net core平台上执行