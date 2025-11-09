<template>
  <div class="chat-container center-narrow py-5">
    <h3 class="text-center main-color-green-text mb-3">智能客服</h3>

    <div class="chat-box bg-light p-3 rounded-4 shadow-sm mb-3">
      <div v-for="(m, i) in messages" :key="i" class="mb-2">
        <div v-if="m.role === 'user'" class="text-end">
          <span class="badge bg-primary text-white">{{ m.text }}</span>
        </div>

        <div v-else class="text-start">
          <div class="bg-white rounded p-2 shadow-sm" v-html="m.content"></div>
        </div>
      </div>
    </div>

    <div class="input-group">
      <input v-model="input" type="text" class="form-control"
             placeholder="請輸入問題..." @keyup.enter="send" />
      <button @click="send" class="btn btn-success">送出</button>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import axios from 'axios'

const input = ref('')
const messages = ref([])

const send = async () => {
  if (!input.value) return
  const msg = input.value
  messages.value.push({ role: 'user', text: msg })
  input.value = ''

  const { data } = await axios.post('/api/cs/chat/ask', { message: msg })
  messages.value.push({ role: 'bot', content: data.content })
}
</script>

<style scoped>
.chat-container { max-width: 600px; }
.chat-box { max-height: 500px; overflow-y: auto; }
</style>
