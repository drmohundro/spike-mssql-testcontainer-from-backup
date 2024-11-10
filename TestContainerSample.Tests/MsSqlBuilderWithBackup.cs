using Docker.DotNet.Models;
using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;

namespace TestContainerSample.Tests;

/// <summary>
/// This relies on the local Dockerfile with the backup...
/// See https://stackoverflow.com/a/69494857/4570
/// </summary>
internal class MsSqlBuilderWithBackup() :
    UnsealedMsSqlBuilder("testcontainer-withbackup");

/// <summary>
/// This is identical to the original MsSqlBuilder, except for two things:
/// 1. It isn't sealed
/// 2. It has a parameter to allow for the image name to be passed in
/// </summary>
internal class UnsealedMsSqlBuilder : ContainerBuilder<UnsealedMsSqlBuilder, MsSqlContainer, MsSqlConfiguration>
{
    private string MsSqlImage { get; }

    public const string DefaultMsSqlImage = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";

    public const ushort MsSqlPort = 1433;

    public const string DefaultDatabase = "master";

    public const string DefaultUsername = "sa";

    public const string DefaultPassword = "yourStrong(!)Password";

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsealedMsSqlBuilder" /> class.
    /// </summary>
    public UnsealedMsSqlBuilder(string msSqlImage = DefaultMsSqlImage)
        : this(new MsSqlConfiguration())
    {
        MsSqlImage = msSqlImage;
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsealedMsSqlBuilder" /> class.
    /// </summary>
    /// <param name="resourceConfiguration">The Docker resource configuration.</param>
    private UnsealedMsSqlBuilder(MsSqlConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
        DockerResourceConfiguration = resourceConfiguration;
    }

    /// <inheritdoc />
    protected override MsSqlConfiguration DockerResourceConfiguration { get; }

    /// <summary>
    /// Sets the MsSql password.
    /// </summary>
    /// <param name="password">The MsSql password.</param>
    /// <returns>A configured instance of <see cref="UnsealedMsSqlBuilder" />.</returns>
    public UnsealedMsSqlBuilder WithPassword(string password)
    {
        return Merge(DockerResourceConfiguration, new MsSqlConfiguration(password: password))
            .WithEnvironment("MSSQL_SA_PASSWORD", password)
            .WithEnvironment("SQLCMDPASSWORD", password);
    }

    /// <inheritdoc />
    public override MsSqlContainer Build()
    {
        Validate();
        return new MsSqlContainer(DockerResourceConfiguration);
    }

    /// <inheritdoc />
    protected override UnsealedMsSqlBuilder Init()
    {
        return base.Init()
            .WithImage(MsSqlImage)
            .WithPortBinding(MsSqlPort, true)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithDatabase(DefaultDatabase)
            .WithUsername(DefaultUsername)
            .WithPassword(DefaultPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitUntil()));
    }

    /// <inheritdoc />
    protected override void Validate()
    {
        base.Validate();

        _ = Guard.Argument(DockerResourceConfiguration.Password, nameof(DockerResourceConfiguration.Password))
            .NotNull()
            .NotEmpty();
    }

    /// <inheritdoc />
    protected override UnsealedMsSqlBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new MsSqlConfiguration(resourceConfiguration));
    }

    /// <inheritdoc />
    protected override UnsealedMsSqlBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new MsSqlConfiguration(resourceConfiguration));
    }

    /// <inheritdoc />
    protected override UnsealedMsSqlBuilder Merge(MsSqlConfiguration oldValue, MsSqlConfiguration newValue)
    {
        return new UnsealedMsSqlBuilder(new MsSqlConfiguration(oldValue, newValue));
    }

    /// <summary>
    /// Sets the MsSql database.
    /// </summary>
    /// <remarks>
    /// The Docker image does not allow to configure the database.
    /// </remarks>
    /// <param name="database">The MsSql database.</param>
    /// <returns>A configured instance of <see cref="UnsealedMsSqlBuilder" />.</returns>
    private UnsealedMsSqlBuilder WithDatabase(string database)
    {
        return Merge(DockerResourceConfiguration, new MsSqlConfiguration(database: database))
            .WithEnvironment("SQLCMDDBNAME", database);
    }

    /// <summary>
    /// Sets the MsSql username.
    /// </summary>
    /// <remarks>
    /// The Docker image does not allow to configure the username.
    /// </remarks>
    /// <param name="username">The MsSql username.</param>
    /// <returns>A configured instance of <see cref="UnsealedMsSqlBuilder" />.</returns>
    private UnsealedMsSqlBuilder WithUsername(string username)
    {
        return Merge(DockerResourceConfiguration, new MsSqlConfiguration(username: username))
            .WithEnvironment("SQLCMDUSER", username);
    }

    /// <inheritdoc cref="IWaitUntil" />
    /// <remarks>
    /// Uses the <c>sqlcmd</c> utility scripting variables to detect readiness of the MsSql container:
    /// https://learn.microsoft.com/en-us/sql/tools/sqlcmd/sqlcmd-utility?view=sql-server-linux-ver15#sqlcmd-scripting-variables.
    /// </remarks>
    private sealed class WaitUntil : IWaitUntil
    {
        /// <inheritdoc />
        public Task<bool> UntilAsync(IContainer container)
        {
            return UntilAsync(container as MsSqlContainer);
        }

        /// <inheritdoc cref="IWaitUntil.UntilAsync" />
        private static async Task<bool> UntilAsync(MsSqlContainer container)
        {
            var sqlCmdFilePath = await container.GetSqlCmdFilePathAsync()
                .ConfigureAwait(false);

            var execResult = await container.ExecAsync(new[] { sqlCmdFilePath, "-C", "-Q", "SELECT 1;" })
                .ConfigureAwait(false);

            return 0L.Equals(execResult.ExitCode);
        }
    }
}