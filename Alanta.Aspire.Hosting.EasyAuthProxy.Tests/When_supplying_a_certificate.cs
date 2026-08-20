using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Shouldly;

namespace Alanta.Aspire.Hosting.EasyAuthProxy.Tests;

// ASPIRECERTIFICATES001: the certificate configuration APIs are still experimental in Aspire 13.5.
#pragma warning disable ASPIRECERTIFICATES001

public class When_supplying_a_certificate
{
    [Theory,
     InlineData(false),
     InlineData(true)]
    public async Task It_should_map_the_certificate_onto_Kestrels_configuration(bool container)
    {
        var proxy = AddProxy(container);

        // Kestrel reads its certificate from configuration, so the whole HTTPS story hinges on
        // these two exact keys - get one wrong and the model still looks fine while the process
        // fails at startup.
        var context = await InvokeCertificateCallbackAsync(proxy, password: "hunter2");

        context.EnvironmentVariables["Kestrel__Certificates__Default__Path"]
            .ShouldBe(context.PfxPath);
        context.EnvironmentVariables["Kestrel__Certificates__Default__Password"]
            .ShouldBe(context.Password);
    }

    [Theory,
     InlineData(false),
     InlineData(true)]
    public async Task It_should_leave_the_password_out_when_the_key_pair_has_none(bool container)
    {
        var proxy = AddProxy(container);

        // An empty Kestrel password setting is not the same as an absent one: Kestrel would try to
        // open the PFX with a blank password instead of no password at all.
        var context = await InvokeCertificateCallbackAsync(proxy, password: null);

        context.EnvironmentVariables.ShouldNotContainKey("Kestrel__Certificates__Default__Password");
    }

    private static IResource AddProxy(bool container)
    {
        var builder = DistributedApplication.CreateBuilder();

        return container
            ? builder.AddEasyAuthProxyContainer("easyauth").Resource
            : builder.AddEasyAuthProxy("easyauth").Resource;
    }

    /// <summary>
    /// Runs the resource's own certificate-configuration callback against a stand-in key pair,
    /// the way Aspire runs it once it has materialised a PFX for the resource.
    /// </summary>
    private static async Task<HttpsCertificateConfigurationCallbackAnnotationContext> InvokeCertificateCallbackAsync(
        IResource resource, string? password)
    {
        var annotation = resource.Annotations
            .OfType<HttpsCertificateConfigurationCallbackAnnotation>()
            .ShouldHaveSingleItem();

        var context = new HttpsCertificateConfigurationCallbackAnnotationContext
        {
            ExecutionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            Resource = resource,
            Arguments = [],
            EnvironmentVariables = [],
            CertificatePath = ReferenceExpression.Create($"/certs/easyauth.crt"),
            KeyPath = ReferenceExpression.Create($"/certs/easyauth.key"),
            CertificateWithKeyPath = ReferenceExpression.Create($"/certs/easyauth.pem"),
            PfxPath = ReferenceExpression.Create($"/certs/easyauth.pfx"),
            Password = password is null ? null : ReferenceExpression.Create($"{password}"),
            CancellationToken = CancellationToken.None
        };

        await annotation.Callback(context);

        return context;
    }
}
