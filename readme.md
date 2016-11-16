Implementations of the ISerialization and IBodyDeserializer interfaces, based on [Json.NET](http://json.codeplex.com/), for [Nancy](http://nancyfx.org)

## Usage

Start of by installing the `Nancy.Serialization.JsonNet` nuget

When Nancy detects that the `JsonNetSerializer` and `JsonNetBodyDeserializer` types are available in the AppDomain, of your application, it will assume you want to use them, rather than the default ones.

### Customization

If you want to customize the behavior of Json.NET, you can provide your own implementation of the `JsonSerializer` type. For example, the following implementation configures Json.NET to use camel-casing and to indent the output

```c#
public class CustomJsonSerializer : JsonSerializer
{
    public CustomJsonSerializer()
    {
        this.ContractResolver = new CamelCasePropertyNamesContractResolver();
        this.Formatting = Formatting.Indented;
    }
}
```

In order for Nancy to know that you want to use the new configuration, you need to register it in your bootstrapper. Here is an example of how you would do that using the `DefaultNancyBootstrapper`

```c#
public class Bootstrapper : DefaultNancyBootstrapper
{
    protected override void ConfigureApplicationContainer(TinyIoCContainer container)
    {
        base.ConfigureApplicationContainer(container);

        container.Register<JsonSerializer, CustomJsonSerializer>();
    }
}
```

## Code of Conduct

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. For more information see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

## Contribution License Agreement

Contributing to Nancy requires you to sign a [contribution license agreement](https://cla2.dotnetfoundation.org/) (CLA) for anything other than a trivial change. By signing the contribution license agreement, the community is free to use your contribution to .NET Foundation projects.

## .NET Foundation

This project is supported by the [.NET Foundation](http://www.dotnetfoundation.org).

## Copyright

Copyright © 2010 Andreas Håkansson, Steven Robbins and contributors

## License

Nancy.Serialization.JsonNet is licensed under [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form"). Refer to license.txt for more information.
