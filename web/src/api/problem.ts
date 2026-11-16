/** An API error (ProblemDetails) with its messages per request field. */
export class ApiProblem extends Error {
  readonly status: number
  readonly fields: Record<string, string>

  constructor(status: number, message: string, fields: Record<string, string> = {}) {
    super(message)
    this.status = status
    this.fields = fields
  }
}

type ProblemBody = { title?: string | null; detail?: string | null; errors?: Record<string, string[]> | null }

function fieldName(key: string) {
  return key.charAt(0).toLowerCase() + key.slice(1)
}

export function toProblem(body: unknown, response: Response): ApiProblem {
  const problem = (body && typeof body === 'object' ? body : {}) as ProblemBody
  const fields: Record<string, string> = {}
  for (const [key, messages] of Object.entries(problem.errors ?? {})) {
    if (messages.length > 0) fields[fieldName(key)] = messages[0]
  }
  const message = problem.detail ?? (response.status === 404 ? 'Not found.' : problem.title) ?? `Request failed (HTTP ${response.status}).`
  return new ApiProblem(response.status, message, fields)
}

/** Returns the data of an API call, or throws an ApiProblem. */
export async function call<T>(request: Promise<{ data?: T; error?: unknown; response: Response }>): Promise<T> {
  const { data, error, response } = await request
  if (!response.ok) throw toProblem(error, response)
  return data as T
}

export const isNotFound = (error: unknown) => error instanceof ApiProblem && error.status === 404
