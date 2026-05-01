import * as vscode from "vscode";

let channel: vscode.LogOutputChannel | undefined;

export function initialize(context: vscode.ExtensionContext): vscode.LogOutputChannel {
  if (!channel) {
    channel = vscode.window.createOutputChannel("Verso", { log: true });
    context.subscriptions.push(channel);
  }
  return channel;
}

function get(): vscode.LogOutputChannel | undefined {
  return channel;
}

export const log = {
  trace(message: string, ...args: unknown[]): void {
    get()?.trace(message, ...args);
  },
  debug(message: string, ...args: unknown[]): void {
    get()?.debug(message, ...args);
  },
  info(message: string, ...args: unknown[]): void {
    get()?.info(message, ...args);
  },
  warn(message: string, ...args: unknown[]): void {
    get()?.warn(message, ...args);
  },
  error(message: string | Error, ...args: unknown[]): void {
    get()?.error(message, ...args);
  },
  /**
   * Appends a raw line without LogOutputChannel's severity/timestamp prefix.
   * Use for piping stdout/stderr from the .NET host so its own log formatting
   * is preserved verbatim.
   */
  appendLine(line: string): void {
    get()?.appendLine(line);
  },
};
