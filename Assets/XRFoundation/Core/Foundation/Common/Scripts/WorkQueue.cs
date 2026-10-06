// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using UnityEngine;



public class WorkQueue : SingletonMonoBehaviour<WorkQueue>
{
	
	public bool IsMainAppThread
	{
		get
		{
			
			return Thread.CurrentThread == _mainAppThread;
		}
	}

	
	
	
	public  void InvokeOnAppThread(Action action)
	{
		if (_mainAppThread != null && IsMainAppThread)
		{
			action();
		}
		else
		{
			lock (queueLock)
			{
				_mainThreadWorkQueue.Enqueue(action);
				
			}
		}
	}

	private object queueLock = new object();

	protected virtual void Awake()
	{
		
		_mainAppThread = Thread.CurrentThread;
	}

	
	protected virtual void Update()
	{
		bool hasResult = true;
		while (hasResult)
		{
			lock (queueLock)
			{
				hasResult = _mainThreadWorkQueue.TryDequeue(out Action workload);
				if (hasResult)
					workload();
			}
		}
	}



	private readonly ConcurrentQueue<Action> _mainThreadWorkQueue = new ConcurrentQueue<Action>();


	private Thread _mainAppThread = null;

}

