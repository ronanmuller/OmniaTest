// Backward-compatibility alias. All existing code referencing SaleCreatedEvent
// without the V1 namespace continues to resolve to the versioned type.
// When V2 is introduced, create V2/SaleCreatedEvent.cs and update only the
// handlers and publishers that need the new fields — V1 consumers are unaffected.
global using SaleCreatedEvent = Ambev.DeveloperEvaluation.Application.Sales.EventHandlers.V1.SaleCreatedEvent;
