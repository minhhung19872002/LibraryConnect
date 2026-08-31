using System.Reflection;
using FluentValidation;
using LibraryConnect.Application.Common.Behaviours;
using LibraryConnect.Application.Common.Security;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers every use-case handler, validator and mapping profile found in this assembly, plus
    /// the cross-cutting MediatR behaviours.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        services.AddScoped<IPasswordPolicyProvider, PasswordPolicyProvider>();
        services.AddScoped<Features.Catalogs.ICatalogUsageService, Features.Catalogs.CatalogUsageService>();
        services.AddScoped<Features.Marc.IMarcRuleProvider, Features.Marc.MarcRuleProvider>();
        services.AddScoped<Features.Cataloging.IBibAuthorityLinker, Features.Cataloging.BibAuthorityLinker>();
        services.AddScoped<Features.Cataloging.IBibRecordWriter, Features.Cataloging.BibRecordWriter>();
        services.AddScoped<Features.Cataloging.IBibDuplicateFinder, Features.Cataloging.BibDuplicateFinder>();
        services.AddScoped<Features.Cataloging.IBibImportRunner, Features.Cataloging.BibImportRunner>();
        services.AddScoped<Features.Cataloging.IBibExcelImportRunner, Features.Cataloging.BibExcelImportRunner>();
        services.AddScoped<Features.Acquisition.IPurchaseDuplicateFinder, Features.Acquisition.PurchaseDuplicateFinder>();
        services.AddScoped<Features.Acquisition.IFormDataBuilder, Features.Acquisition.FormDataBuilder>();

        return services;
    }
}
