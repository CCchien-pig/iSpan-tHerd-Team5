export type MemberRank = {
  MemberRankId: string
  RankName: string
  RebateRate: number
  RankDescription?: string | null
}

export type ProfileDto = {
  id: string
  email: string
  name: string
  userNumberId: number
  roles: string[]
  MemberRankId?: string
  ReferralCode?: string | null
  UsedReferralCode?: string | null
  CreatedDate?: string | null
  LastLoginDate?: string | null
  Gender?: string | null
  Address?: string | null
  IsActive?: boolean
}
