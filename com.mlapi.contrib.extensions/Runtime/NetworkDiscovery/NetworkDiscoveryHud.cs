﻿using System;
using System.Collections.Generic;
using System.Net;
using MLAPI;
using MLAPI.Transports.UNET;
using UnityEngine;
using Object = UnityEngine.Object;

[RequireComponent(typeof(NetworkDiscovery))]
[RequireComponent(typeof(NetworkManager))]
public class NetworkDiscoveryHud : MonoBehaviour
{
    NetworkDiscovery m_Discovery;
    NetworkManager m_NetworkManager;

    Dictionary<string, (IPEndPoint, DiscoveryResponseData)> discoveredServers = new Dictionary<string, (IPEndPoint, DiscoveryResponseData)>();

    public Vector2 DrawOffset = new Vector2(10, 210);

    void Awake()
    {
        m_Discovery = GetComponent<NetworkDiscovery>();
        m_NetworkManager = GetComponent<NetworkManager>();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (m_Discovery == null)
        {
            m_Discovery = GetComponent<NetworkDiscovery>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(m_Discovery.OnServerFound, OnServerFound);
            UnityEditor.Undo.RecordObjects(new Object[] { this, m_Discovery}, "Set NetworkDiscovery");
        }
    }
#endif

    void OnServerFound(IPEndPoint sender, DiscoveryResponseData response)
    {
        discoveredServers[response.ServerName] = (sender, response);
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(DrawOffset, new Vector2(200, 600)));

        if (m_NetworkManager.IsServer || m_NetworkManager.IsClient)
        {
            if (m_NetworkManager.IsServer)
            {
                ServerControlsGUI();
            }
        }
        else
        {
            ClientSearchGUI();
        }

        GUILayout.EndArea();
    }

    void ClientSearchGUI()
    {
        if (m_Discovery.IsRunning)
        {
            if (GUILayout.Button("Stop Client Discovery"))
            {
                m_Discovery.StopDiscovery();
                discoveredServers.Clear();
            }
        }
        else
        {
            if (GUILayout.Button("Discover Servers"))
            {
                m_Discovery.StartClient();
                m_Discovery.ClientBroadcast(new DiscoveryBroadcastData { UniqueApplicationId = m_Discovery.UniqueApplicationId });
            }
        }
    }

    void ServerControlsGUI()
    {
        if (m_Discovery.IsRunning)
        {
            if (GUILayout.Button("Stop Server Discovery"))
            {
                m_Discovery.StopDiscovery();
            }
            if (GUILayout.Button("Refresh List"))
            {
                discoveredServers.Clear();
                m_Discovery.ClientBroadcast(new DiscoveryBroadcastData { UniqueApplicationId = m_Discovery.UniqueApplicationId });
            }

            foreach (var discoveredServer in discoveredServers)
            {
                if (GUILayout.Button(discoveredServer.Key))
                {
                    UNetTransport transport = (UNetTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
                    transport.ConnectAddress = discoveredServer.Value.Item1.Address.ToString();
                    transport.ConnectPort = discoveredServer.Value.Item2.Port;
                    m_NetworkManager.StartClient();
                }
            }
        }
        else
        {
            if (GUILayout.Button("Start Server Discovery"))
            {
                m_Discovery.StartServer();
            }
        }
    }
}
