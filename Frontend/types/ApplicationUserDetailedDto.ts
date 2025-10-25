export interface ApplicationUserDetailedDto {
  id: string
  userName: string
  email?: string | null
  emailConfirmed: boolean
  phoneNumber?: string | null
  phoneNumberConfirmed: boolean

  // Personal name fields
  name?: string | null
  surname?: string | null
  fullName: string

  // Application-specific fields
  saldo: number
  lastOrdered: string | null

  // Profile picture encoded as base64 (nullable)
  profilePictureBase64?: string | null
}
