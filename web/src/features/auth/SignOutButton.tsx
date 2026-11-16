import { useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { signOut } from '@/features/auth/session'

export function SignOutButton() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  async function handleClick() {
    await signOut()
    queryClient.clear()
    navigate('/login', { replace: true })
  }

  return (
    <Button variant="outline" size="sm" onClick={handleClick}>
      Sign out
    </Button>
  )
}
