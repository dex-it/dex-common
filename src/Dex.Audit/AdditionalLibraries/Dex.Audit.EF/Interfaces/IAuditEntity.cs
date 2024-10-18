namespace Dex.Audit.EF.Interfaces;

/// <summary>
/// An interface for marking entities that need to be audited.
/// </summary>
/// <remarks>
/// Entity changes will be auditioned only if DbContext has already started it.
/// If a failure occurred before it got into the DbContext tracker, then an audit message is generated and will not be sent.
/// </remarks>
public interface IAuditEntity;
