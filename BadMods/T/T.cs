/*
 * Harmony for Cities Skylines
 *  Copyright (C) 2021 Radu Hociung <radu.csmods@ohmi.org>
 *  
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the modified GNU General Public License as
 *  published in the root directory of the source distribution.
 *  
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  modified GNU General Public License for more details.
 *  
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */
public class Class1 : ICities.IUserMod
{
	public string Name => "T";
	public string Description => "This mod is brought to you by the letter 'T'";


	/* This mod demostrates the vulnerability described at 
	 * https://github.com/drok/Harmony-CitiesSkylines/issues/18
	 * 
	 * Suppose the following 3 mods are installed, and are subscribed
	 * in this order:
	 * 
	 * HideCrosswalks
	 * TrafficManager
	 * T
	 * 
	 * They will be loaded in this order, and they will appear
	 * in the AppDomain like this (other non-involved assemblies
	 * omitted for clarity:
	 * 
	 * T, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
	 * 	[...]
	 * TrafficManager, Version=11.5.2.29114, Culture=neutral, PublicKeyToken=null
	 * TMPE.CitiesGameBridge, Version=11.5.2.29113, Culture=neutral, PublicKeyToken=null
	 * TMPE.API, Version=11.5.2.29113, Culture=neutral, PublicKeyToken=null
	 * TMPE.GenericGameBridge, Version=11.5.2.29112, Culture=neutral, PublicKeyToken=null
	 * 	[...]
	 * HideCrosswalks, Version=3.0.3.37975, Culture=neutral, PublicKeyToken=null
	 * 
	 * When HideCroswalks refers to TrafficManager, the CSL resolver searches the
	 * list of loaded assemblies, finds T first, as a match for TrafficManager and
	 * the other assemblies starting with the letter "T", and returns the T 
	 * assembly as the result. The CSL resolver is useless at logging this, and logs:
	 * 
	 * Assembly 'TrafficManager, Version=1.0.7266.28335, Culture=neutral, PublicKeyToken=null' resolved to ''  [Serialization]
	 * 
	 * The Harmony resolver logs:
	 * [Harmony 0.9-DEBUG] WARNING 'Permit exploiting game's resolver vulnerability' is deprecated; Will be obsolete at Harmony 1.1.0.0
	 * [Harmony 0.9-DEBUG] INFO - Assembly Resolve (requested by HideCrosswalks[3.0.3.37975], exploit!):
	 *     Requested:   TrafficManager, Version=1.0.7266.28335, Culture=neutral, PublicKeyToken=null
	 *     Resolved as: T, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
	 * 
	 * In this way, HideCrosswalks and other mods that rely on Traffic manager, but mismatching
	 * the version, require the vulnerable resolver to be activated and enable this exploit.
	 * After being mismatched to T instead of TrafficManager, these mods will not do what's
	 * expected. In my experience, they fail silently, giving no indication that something is
	 * wrong.
	 * 
	 * Note that the "T" mod needs not be enabled, it is loaded anyway, causing this
	 * behaviour.
	 * 
	 * The Harmony mod detects this condition, and alerts that it will behave like the
	 * vulnerable resolver until it reaches version 1.1.0.0, at which time emulating this
	 * buggy behaviour will end, and mods that depend on TrafficManager (and others)
	 * will not be bound to the TrafficManager unless they correclty specify the version they
	 * seek.
	 */
}
