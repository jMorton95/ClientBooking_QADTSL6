namespace ClientBooking.Authentication;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class AllowAnonymousAttribute : Attribute
{
}
