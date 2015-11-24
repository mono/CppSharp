function cat(file)
  local file = assert(io.open(file, "r"))
  local output = file:read('*all')
  file:close()
  return output
end

function execute(cmd)
  print(cmd)
  local file = assert(io.popen(cmd, "r"))
  local output = file:read('*all')
  file:close()
  return output
end

local git = {}

function git.clone(dir, url, target)
  local cmd = "git clone " .. url .. " " .. path.translate(dir) 
  if target ~= nil then
    cmd = cmd .. " " .. target
  end
  execute(cmd)
end

function git.checkout(dir, rev)
  local cmd = "git -C " .. path.translate(dir) .. " checkout " .. rev
  execute(cmd)
end

function git.revision(dir)
  local cmd = "git -C " .. path.translate(dir) .. " checkout " .. rev
  execute(cmd)
end

function http.progress (total, curr)
  local ratio = curr / total;
  ratio = math.floor(math.min(math.max(ratio, 0), 1));

  local percent = ratio * 100;
  print("Download progress (" .. percent .. "%/100%)")
end

function download(url, file)
  print("Downloading file " .. file)
  local res = http.download(url, file, http.progress)

  if res == nil then
    error("Error downloading file " .. file)
  end
end