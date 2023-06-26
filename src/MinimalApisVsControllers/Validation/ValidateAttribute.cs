namespace MinimalApisVsControllers;


//credit: https://benfoster.io/blog/minimal-api-validation-endpoint-filters/

[AttributeUsage(AttributeTargets.Parameter)]
public class ValidateAttribute : Attribute
{
}