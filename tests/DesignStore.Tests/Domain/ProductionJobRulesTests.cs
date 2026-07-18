using DesignStore.Domain.Commerce;
namespace DesignStore.Tests.Domain;
public sealed class ProductionJobRulesTests
{
    [Fact] public void Production_follows_forward_workflow(){Assert.True(ProductionJobRules.CanTransition(ProductionJobStatus.Planned,ProductionJobStatus.InProgress));Assert.True(ProductionJobRules.CanTransition(ProductionJobStatus.InProgress,ProductionJobStatus.QualityCheck));Assert.True(ProductionJobRules.CanTransition(ProductionJobStatus.Ready,ProductionJobStatus.Shipped));}
    [Fact] public void Completed_job_cannot_be_reopened()=>Assert.False(ProductionJobRules.CanTransition(ProductionJobStatus.Completed,ProductionJobStatus.InProgress));
}
