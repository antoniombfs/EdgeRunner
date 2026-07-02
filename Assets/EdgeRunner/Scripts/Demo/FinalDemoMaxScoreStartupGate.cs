using System.Collections;
using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// Gives every MaxScore scene object one full frame to finish Awake before the
/// inference agent can request its first episode reset. This is intentionally
/// demo-only and leaves the validated ObjectAware agent implementation intact.
/// </summary>
public sealed class FinalDemoMaxScoreStartupGate : MonoBehaviour
{
    [SerializeField] private EdgeRunnerAgentV5ScoreMaxObjectAware agent;
    [SerializeField] private DecisionRequester decisionRequester;

    public EdgeRunnerAgentV5ScoreMaxObjectAware Agent => agent;
    public DecisionRequester DecisionRequester => decisionRequester;

    public void Configure(
        EdgeRunnerAgentV5ScoreMaxObjectAware targetAgent,
        DecisionRequester targetDecisionRequester)
    {
        agent = targetAgent;
        decisionRequester = targetDecisionRequester;

        if (agent != null)
        {
            agent.enabled = false;
        }
        if (decisionRequester != null)
        {
            decisionRequester.enabled = false;
        }
    }

    private IEnumerator Start()
    {
        yield return null;

        if (agent != null)
        {
            agent.enabled = true;
        }

        yield return null;

        if (decisionRequester != null)
        {
            decisionRequester.enabled = true;
        }

        enabled = false;
    }
}
