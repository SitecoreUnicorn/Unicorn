UNICORN.ROLES README

Thanks for installing Unicorn.Roles! Here are some tips to get you started:

First, you should set up a configuration. This can either be an existing Unicorn configuration that includes items, or a roles-only configuration at your discretion.
Make a copy of App_Config/Include/Unicorn/Unicorn.Configs.Default.Roles.config.example and rename it to .config. You can place this file anywhere in App_Config/Include, such as Include/MySite/Unicorn.Configs.MySiteSecurity.config.
Review the comments in the example configuration file and edit the values as you see fit (especially the rolePredicate settings, which control what is included).

Run a build, so that the Unicorn.Roles assemblies are copied to your bin folder. If you develop out of webroot you may need to deploy or something as well.
Open the Unicorn control panel (defaults to /unicorn.aspx)
In the control panel, click the Reserialize button on your role configuration to make sure all of the roles you want to keep synced are written to disk.

Check your serialized roles into source control, and use the Sync in the control panel to move updates from others into your database.

Have questions? Tweet @kamsar or @cassidydotdk, or join Sitecore Community Slack and ask in #unicorn

Found a bug? Send me a pull request on GitHub if you're feeling awesome: https://github.com/SitecoreUnicorn/Unicorn
(or an issue if you're feeling lazy)