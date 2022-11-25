﻿using AutoCoordinator.GameConsole;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace AutoCoordinator.Game
{
	public class PokemonGame
	{
		private PokemonGameHeader m_Header;
		private ConsoleConnection m_Connection;

		private uint m_AutoBufferSize;
		private uint m_AutoBufferAddr;
		private ushort m_CommandCounter;

		public enum GameInputState
		{
			Unknown,
			TitleMenu,
			Overworld,
			Battle,
		}

		public enum CommandCode
		{
			ClearPlayerParty = 0,
			ClearEnemyParty,
			SetPlayerMon,
			SetEnemyMon,
			SetPlayerMonData,
			SetEnemyMonData,
			StartTrainerBattle,
			GetInputState,
			GetNumSpecies,
			ApplyRandomPlayerMonPreset,
			ApplyRandomEnemyMonPreset,
		}

		public PokemonGame()
		{
			m_Connection = new ConsoleConnection(new IPEndPoint(IPAddress.Loopback, 30150));
		}

		public ConsoleConnection Connection
		{
			get => m_Connection;
		}

		public bool CheckConnection()
		{
			if (PokemonGameHeader.TryParse(m_Connection, out m_Header))
			{
				m_AutoBufferSize = m_Connection.Cmd_Emu_Read32(m_Header.AutomationHeaderAddr + 0);
				m_AutoBufferAddr = m_Connection.Cmd_Emu_Read32(m_Header.AutomationHeaderAddr + 4);
				return true;
			}

			return false;
		}

		private bool PushCmd(CommandCode cmd, params int[] values)
		{
			if (values.Length >= m_AutoBufferSize - 2)
				throw new Exception("Too many params for communication buffer");

			//Console.WriteLine("Cmd: " + cmd + " " + string.Join(" ", values));

			m_Connection.Cmd_Emu_Write16(m_AutoBufferAddr + 2, (ushort)cmd);

			for(uint i = 0; i < values.Length; ++i)
				m_Connection.Cmd_Emu_Write16(m_AutoBufferAddr + 4 + 2 * i, (ushort)values[i]);

			// Update the counter last, as the game is now allowed to issue the command
			m_Connection.Cmd_Emu_Write16(m_AutoBufferAddr + 0, ++m_CommandCounter);

			// Wait for game to complete task
			for (int i = 0; i < 1000; ++i)
			{
				Thread.Sleep(100);
				ushort counterValue = m_Connection.Cmd_Emu_Read16(m_AutoBufferAddr + 0);

				// Wait until the counter is equal to counter + 1 to indicate that the command has been executed
				if (counterValue == m_CommandCounter + 1)
				{
					// Success!
					m_CommandCounter = counterValue;
					return true;
				}
			}

			Console.Error.WriteLine($"Cmd {cmd} timeout");
			return false;
		}

		public ushort ReadReturnValue(int offset = 0)
		{
			return m_Connection.Cmd_Emu_Read16(m_AutoBufferAddr + 4 + 2 * (uint)offset);
		}

		public void ResetGame()
		{
			m_Connection.Cmd_Emu_Reset();
			Thread.Sleep(100);
		}

		public void ClearPlayerParty()
		{
			PushCmd(CommandCode.ClearPlayerParty);
		}

		public void ClearEnemyParty()
		{
			PushCmd(CommandCode.ClearEnemyParty);
		}

		public void SetPlayerMon(int index, int species, int level, int fixedIV)
		{
			PushCmd(CommandCode.SetPlayerMon, index, species, level, fixedIV);
		}

		public void SetEnemyMon(int index, int species, int level, int fixedIV)
		{
			PushCmd(CommandCode.SetEnemyMon, index, species, level, fixedIV);
		}

		public void SetPlayerMonData(int index, PokemonDataID dataId, int value)
		{
			PushCmd(CommandCode.SetPlayerMonData, index, (int)dataId, value);
		}

		public void SetEnemyMonData(int index, PokemonDataID dataId, int value)
		{
			PushCmd(CommandCode.SetEnemyMonData, index, (int)dataId, value);
		}

		public void StartTrainerBattle(bool isDoubleBattle = false)
		{
			PushCmd(CommandCode.StartTrainerBattle);
		}

		public GameInputState GetInputState()
		{
			if (PushCmd(CommandCode.GetInputState))
				return (GameInputState)ReadReturnValue();

			return GameInputState.Unknown;
		}
		public int GetNumSpecies()
		{
			if (PushCmd(CommandCode.GetNumSpecies))
				return ReadReturnValue();

			return 0;
		}

		public void ApplyRandomPlayerMonPreset(int index)
		{
			PushCmd(CommandCode.ApplyRandomPlayerMonPreset, index);
		}

		public void ApplyRandomEnemyMonPreset(int index)
		{
			PushCmd(CommandCode.ApplyRandomEnemyMonPreset, index);
		}
	}
}
