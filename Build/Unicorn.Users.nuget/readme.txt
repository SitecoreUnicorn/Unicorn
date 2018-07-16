UNICORN.USERS README

Thanks for installing Unicorn.Users! Here are some tips to get you started:

First, you should set up a configuration. This can either be an existing Unicorn configuration that includes items, or a users-only configuration at your discretion. 
You may colocate items, roles, and users or any combination thereof in a single configuration.
Make a copy of App_Config/Include/Unicorn/Unicorn.Configs.Default.Users.config.example and rename it to .config. You can place this file anywhere in App_Config/Include, such as Include/MySite/Unicorn.Configs.MySiteSecurity.config.
Review the comments in the example configuration file and edit the values as you see fit (especially the userPredicate settings, which control what is included).

Run a build, so that the Unicorn.Users assemblies are copied to your bin folder. If you develop out of webroot you may need to deploy or something as well.
Open the Unicorn control panel (defaults to /unicorn.aspx)
In the control panel, click the Reserialize button on your users configuration to make sure all of the users you want to keep synced are written to disk.

Check your serialized users into source control, and use the Sync in the control panel to move updates from others into your database.

HINTS

Passwords: By default newly created users from serialization are assigned random 32-character passwords. Reset these to allow them to sign in. Users that already exist do not have their passwords altered.
Property syncing: Certain user properties, such as last login time, are not serialized because they should be kept environment-specific.
Preference syncing: All user profile settings are serialized. This may cause confusion as it could un-set content editor preferences when synced (such as visible ribbon tabs).

Have questions? Tweet @kamsar or @cassidydotdk, or join Sitecore Community Slack and ask in #unicorn

Found a bug? Send me a pull request on GitHub if you're feeling awesome: https://github.com/SitecoreUnicorn/Unicorn
(or an issue if you're feeling lazy)