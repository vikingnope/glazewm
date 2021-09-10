﻿using System;
using System.Text.RegularExpressions;
using LarsWM.Domain.Common.Enums;
using LarsWM.Domain.Containers;
using LarsWM.Domain.Containers.Commands;
using LarsWM.Domain.UserConfigs.Commands;
using LarsWM.Domain.Windows.Commands;
using LarsWM.Domain.Workspaces.Commands;
using LarsWM.Infrastructure.Bussing;
using LarsWM.Infrastructure.WindowsApi;

namespace LarsWM.Domain.UserConfigs.CommandHandlers
{
  class RegisterKeybindingsHandler : ICommandHandler<RegisterKeybindingsCommand>
  {
    private Bus _bus;
    private ContainerService _containerService;
    private KeybindingService _keybindingService;

    public RegisterKeybindingsHandler(Bus bus, ContainerService containerService, KeybindingService keybindingService)
    {
      _bus = bus;
      _containerService = containerService;
      _keybindingService = keybindingService;
    }

    public CommandResponse Handle(RegisterKeybindingsCommand command)
    {
      foreach (var keybinding in command.Keybindings)
      {
        var commandName = FormatCommandName(keybinding.Command);

        Command parsedCommand = null;
        try
        {
          parsedCommand = ParseCommand(commandName);
        }
        catch
        {
          throw new Exception($"Invalid command {commandName}.");
        }

        foreach (var binding in keybinding.Bindings)
          // Use `CommandResponse` to resolve the command type at runtime and allow multiple dispatch.
          _keybindingService.AddGlobalKeybinding(binding, () => _bus.Invoke((dynamic)parsedCommand));
      }

      return CommandResponse.Ok;
    }

    private string FormatCommandName(string commandName)
    {
      var formattedCommandString = commandName.Trim().ToLowerInvariant();
      return Regex.Replace(formattedCommandString, @"\s+", " ");
    }

    private Command ParseCommand(string commandName)
    {
      var commandParts = commandName.Split(" ");

      return commandParts[0] switch
      {
        "layout" => ParseLayoutCommand(commandParts),
        "focus" => ParseFocusCommand(commandParts),
        "move" => ParseMoveCommand(commandParts),
        "resize" => ParseResizeCommand(commandParts),
        "close" => new CloseFocusedWindowCommand(),
        _ => throw new ArgumentException(),
      };
    }

    private Command ParseLayoutCommand(string[] commandParts)
    {
      return commandParts[1] switch
      {
        "vertical" => new ChangeContainerLayoutCommand(Layout.VERTICAL),
        "horizontal" => new ChangeContainerLayoutCommand(Layout.HORIZONTAL),
        _ => throw new ArgumentException(),
      };
    }

    // TODO: Return focus command once implemented.
    private Command ParseFocusCommand(string[] commandParts)
    {
      return commandParts[1] switch
      {
        "left" => new FocusWorkspaceCommand("1"),
        "right" => new FocusWorkspaceCommand("1"),
        "up" => new FocusWorkspaceCommand("1"),
        "down" => new FocusWorkspaceCommand("1"),
        // TODO: Validate workspace name.
        "workspace" => new FocusWorkspaceCommand(commandParts[2]),
        _ => throw new ArgumentException(),
      };
    }

    private Command ParseMoveCommand(string[] commandParts)
    {
      return commandParts[1] switch
      {
        "left" => new MoveFocusedWindowCommand(Direction.LEFT),
        "right" => new MoveFocusedWindowCommand(Direction.RIGHT),
        "up" => new MoveFocusedWindowCommand(Direction.UP),
        "down" => new MoveFocusedWindowCommand(Direction.DOWN),
        // TODO: Return move to workspace command once implemented.
        "to" => new FocusWorkspaceCommand("1"),
        _ => throw new ArgumentException(),
      };
    }

    private Command ParseResizeCommand(string[] commandParts)
    {
      return commandParts[1] switch
      {
        "grow" => commandParts[2] switch
        {
          "height" => new ResizeFocusedWindowCommand(ResizeDirection.GROW_HEIGHT),
          "width" => new ResizeFocusedWindowCommand(ResizeDirection.GROW_WIDTH),
          _ => throw new ArgumentException(),
        },
        "shrink" => commandParts[2] switch
        {
          "height" => new ResizeFocusedWindowCommand(ResizeDirection.SHRINK_HEIGHT),
          "width" => new ResizeFocusedWindowCommand(ResizeDirection.SHRINK_WIDTH),
          _ => throw new ArgumentException(),
        },
        _ => throw new ArgumentException(),
      };
    }
  }
}